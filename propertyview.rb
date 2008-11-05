require 'gtk2'

module Cpg
	class PropertyView < Gtk::VBox
		type_register

		def initialize(obj = nil)
			super({'homogeneous' => false, 'spacing' => 3})

			init(obj)
			show_all
		end
	
		def new_button(stock)
			but = Gtk::ToolButton.new(stock)
		
			but.signal_connect('clicked') { |but| yield }
			but
		end
	
		def init(obj)
			clear

			@signals = []
			@object = obj

			while self.children.length > 0
				self.remove(self.children[0])
			end
			
			vw = Gtk::ScrolledWindow.new
			vw.set_policy(Gtk::POLICY_AUTOMATIC, Gtk::POLICY_AUTOMATIC)
		
			@store = Gtk::ListStore.new(String, String)
			tv = Gtk::TreeView.new(@store)

			vw << tv
		
			tv.show
			vw.show
		
			@treeview = tv
			self.pack_end(vw, true, true, 0)			

			column = Gtk::TreeViewColumn.new('Name', Gtk::CellRendererText.new, {'text' => 0})
			column.resizable = true
			column.min_width = 75
		
			tv.append_column(column)
		
			renderer = Gtk::CellRendererText.new
			
			if @object		
				renderer.signal_connect('edited') do |renderer,path,new_text|
					piter = @store.get_iter(path)
					piter[1] = new_text
			
					@object.set_property(piter[0], new_text)
				end
			end
		
			column = Gtk::TreeViewColumn.new('Value', renderer)
			column.resizable = true
			column.set_cell_data_func(renderer) do |column, cell, model, piter|
				cell.text = piter[1]
				cell.editable = @object && !@object.read_only?(piter[0])
			end
			column.min_width = 100
			tv.append_column(column)
		
			if @object
				renderer = Gtk::CellRendererText.new
				renderer.signal_connect('edited') do |renderer,path,new_text|
					piter = @store.get_iter(path)
			
					@object.set_initial_value(piter[0], new_text)
				end

				column = Gtk::TreeViewColumn.new('Initial', renderer)
				column.resizable = true
				column.set_cell_data_func(renderer) do |column, cell, model, piter|
					cell.text =  @object.initial_value(piter[0]).to_s
					cell.editable = !@object.read_only?(piter[0]) && piter[0].to_sym != :id
				end
				column.min_width = 30
				tv.append_column(column)

				if @object.is_a?(Components::SimulatedObject)
					renderer = Gtk::CellRendererToggle.new
		
					renderer.signal_connect('toggled') do |renderer,path,new_text|
						piter = @store.get_iter(path)			
						na = (not renderer.active?)

						@object.set_integrated(piter[0], na)
					end

					column = Gtk::TreeViewColumn.new('Int', renderer)
					column.resizable = true
					column.set_cell_data_func(renderer) do |column, cell, model, piter|
						cell.active = @object.integrated?(piter[0])
						cell.activatable = !@object.read_only?(piter[0]) && piter[0].to_sym != :id
					end
					column.max_width = 30
		
					tv.append_column(column)
				end

				renderer = Gtk::CellRendererText.new
				renderer.signal_connect('edited') do |renderer,path,new_text|
					piter = @store.get_iter(path)
					@object.set_range(piter[0], new_text)
				end
			
				column = Gtk::TreeViewColumn.new('Range', renderer)
				column.resizable = true
				column.set_cell_data_func(renderer) do |column, cell, model, piter|
					cell.text = @object.get_range(piter[0]).to_s
					cell.editable = !(@object.read_only?(piter[0] || @object.invisible?(piter[0])) || piter[0].to_sym == :id)
				end
				column.max_width = 100
		
				tv.append_column(column)
			end

			if @object && @object.is_a?(Serialize::Dynamic)
				hbox = Gtk::HBox.new(false, 3)
				self.pack_start(hbox, false, false, 0)
		
				entry = Gtk::Entry.new
				hbox.pack_start(entry, true, true, 0)
			
				entry.signal_connect('key_press_event') do |entry, ev|
					if ev.keyval == Gdk::Keyval::GDK_Return
						do_add_property(entry)
						entry.select_region(0, -1)
					end
				end
			
				tv.signal_connect('key_press_event') do |tv, ev|
					if ev.keyval == Gdk::Keyval::GDK_Delete
						do_remove_property
					end
				end
			
				but = new_button(Gtk::Stock::ADD) { do_add_property(entry) }
				hbox.pack_start(but, false, false, 0)
				 
				but = new_button(Gtk::Stock::REMOVE) { do_remove_property }
				but.sensitive = false
				hbox.pack_start(but, false, false, 0)

				tv.selection.signal_connect('changed') do |sel|
					but.sensitive = sel.selected && @object.dynamic_property?(sel.selected[0])
				end
			
				hbox.show_all
			end

			if @object
				@signals << @object.signal_connect('property_changed') do |obj, prop|
					property_changed(prop)
				end
				
				@signals << @object.signal_connect('property_removed') do |obj, prop|
					property_removed(prop)
				end
				
				@signals << @object.signal_connect('property_added') do |obj, prop|
					property_added(prop)
				end

				@signals << @object.signal_connect('range_changed') do |obj, prop|
					range_changed(prop)
				end

				@signals << @object.signal_connect('initial_changed') do |obj, prop|
					initial_changed(prop)
				end
			
				init_store		
				self.sensitive = true
			end
		end
	
		def find_property(prop)
			@store.each do |piter|
				return piter[2] if piter[2][0] == prop
			end
		
			return nil
		end	
		
		def initial_changed(prop)
			piter = find_property(prop)
		
			return unless piter
			@store.row_changed(piter.path, piter)
		end
		
		def range_changed(prop)
			piter = find_property(prop)
		
			return unless piter
			@store.row_changed(piter.path, piter)
		end
	
		def property_changed(prop)
			piter = find_property(prop)
		
			return unless piter
			piter[1] = @object.get_property(prop).to_s
		end
		
		def property_added(prop)
			add_property(prop)
		end
		
		def property_removed(prop)
			piter = find_property(prop)
			
			if piter
				@store.remove(piter)
			end
		end
	
		def add_property(prop)
			piter = @store.append
			piter[0] = prop
		
			v = @object.get_property(prop)
			piter[1] = v ? v.to_s : ''
		end
	
		def do_add_property(entry)
			s = entry.text
		
			return if s.empty? or !(s =~ /^[a-zA-Z][a-zA-Z0-9_]*$/)

			# see if the property already exists
			return if @object.properties.include?(s.to_sym)
			@object.set_property(s.to_sym, nil)
		end
	
		def do_remove_property
			piter = @treeview.selection.selected

			return unless piter
			return unless @object.dynamic_property?(piter[0])
		
			@object.remove_property(piter[0])
		end
	
		def init_store
			@object.properties.each do |prop|
				next if @object.invisible?(prop)
				add_property(prop)
			end
		end
		
		def clear
			@signals.each { |signal| @object.signal_handler_disconnect(signal) } if @object
			@signals = []
			
			@store.clear if @store
			@object = nil
			
			self.sensitive = false unless destroyed?
		end
	
		def signal_do_destroy
			clear
		end
	end
end
