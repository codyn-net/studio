require 'gtk2'
require 'grid'
require 'propertyeditor'
require 'propertyview'
require 'loader'
require 'saver'
require 'monitor'
require 'control'
require 'messagearea'
require 'simulation'
require 'stock'
require 'flatformat'
require 'serialize/object'
require 'serialize/array'
require 'set'

module Cpg
	class Cpg < Array
		include Serialize::Object
		include Enumerable
		include SortedArray

		property :allocation, :zoom, :pane_position, :period, :monitors, :controls
		property :monitors_size, :controls_size

		def initialize(objects = nil)
			super(objects ? objects : 0)
	
			self.allocation = Allocation.new([nil, nil, nil, nil])
			self.pane_position = 400
			self.period = ''
			self.monitors = Serialize::Array.new
			self.controls = Serialize::Array.new
			
			self.monitors_size = Serialize::Array.new([1, 1])
			self.controls_size = Serialize::Array.new([1, 1])

			sort!
		end
	end

	class Application < Gtk::Window
		attr_reader :grid

		class CutAndPaste
			XML = 1

			TARGETS = [
					['text/plain', 0, CutAndPaste::XML],
					['UTF8_STRING', 0, CutAndPaste::XML],
					['TEXT', 0, CutAndPaste::XML],
					['COMPOUND_TEXT', 0, CutAndPaste::XML],
					['text/plain;charset=utf-8', 0, CutAndPaste::XML],
				]
		end

		type_register

		def self.new(*args)
			if not @instance
				@instance = super
			end
			
			@instance
		end
		
		def self.instance
			@instance
		end
		
		def initialize
			super
		
			set_default_size(700, 600)
		
			@filename = nil
			@monitor = nil
			@control = nil
			@property_editors = {}
			@simulation_paused = false
			@modified = false

			@simulation = Simulation.new(nil, 0.05)
			
			build
			show_all
					
			clear
			
			update_title
		end
	
		def tool_button(stock)
			button = Gtk::ToolButton.new(stock)

			button.signal_connect('clicked') { yield }
			button
		end
	
		def icon_path(name)
			File.join(File.dirname(__FILE__), 'icons', name)
		end
		
		def traverse_templates(uiman, group, mid, path, parent)
			return unless File.directory?(path)

			pname = parent.gsub('/', '')
			
			Dir.entries(path).each do |f|
				next if (f == '.' || f == '..' || f[0] == ?.)
				
				full = File.join(path, f)
			
				if File.directory?(full)
					name = "#{pname}#{f.capitalize}Menu"
					
					group.add_actions([
						["#{name}Action", nil, f.capitalize, nil, nil, nil]
					])
					
					uiman.add_ui(mid, "/menubar/InsertMenu/#{parent}", name, "#{name}Action", Gtk::UIManager::MENU, false)
					uiman.add_ui(mid, "/popup/InsertMenu/#{parent}", name, "#{name}Action", Gtk::UIManager::MENU, false)
					
					traverse_templates(uiman, group, mid, full, "#{parent}/#{name}")
					next
				end
				
				next unless File.file?(full)
				next unless f =~ /\.cpg$/

				name = f.gsub(/\.cpg$/, '').gsub(/[\s.-:]+/, '').capitalize

				group.add_actions([
					["#{pname}#{name}Action", nil, name, nil, '', Proc.new { |g,a| import_from_file(full)}]
				])

				uiman.add_ui(mid, "/menubar/InsertMenu/#{parent}", "#{pname}#{name}", "#{pname}#{name}Action", Gtk::UIManager::MENUITEM, false)
				uiman.add_ui(mid, "/popup/InsertMenu/#{parent}", "#{pname}#{name}", "#{pname}#{name}Action", Gtk::UIManager::MENUITEM, false)
			end
		end
		
		def build_templates(uiman)
			tdir = File.join(File.dirname(__FILE__), 'templates')
			group = Gtk::ActionGroup.new('TemplateActions')
			mid = uiman.new_merge_id
			uiman.insert_action_group(group, 0)
			
			traverse_templates(uiman, group, mid, tdir, 'InsertTemplates')
		end
	
		def build
			vbox = Gtk::VBox.new(false, 0)
			hbox = Gtk::HBox.new(false, 6)
		
			@uimanager = Gtk::UIManager.new
			ag = Gtk::ActionGroup.new('NormalActions')
			ag.add_actions([
				['FileMenuAction', nil, '_File', nil, nil, nil],
				['NewAction', Gtk::Stock::NEW, nil, nil, 'New CPG network', Proc.new { |g,a| do_new }],
				['OpenAction', Gtk::Stock::OPEN, nil, nil, 'Open CPG network', Proc.new { |g,a| do_open }],
				['RevertAction', Gtk::Stock::REVERT_TO_SAVED, nil, nil, 'Revert changes', Proc.new { |g,a| do_revert }],
				['SaveAction', Gtk::Stock::SAVE, nil, nil, 'Save CPG file', Proc.new { |g,a| do_save }],
				['SaveAsAction', Gtk::Stock::SAVE_AS, nil, '<Control><Shift>S', 'Save CPG file', Proc.new { |g,a| do_save_as }],
				
				['ImportAction', nil, 'Import', nil, 'Import CPG network objects', Proc.new { |g,a| do_import }],
				['ExportAction', nil, 'Export', '<Control>e', 'Export CPG network objects', Proc.new { |g,a| do_export }],
				
				['QuitAction', Gtk::Stock::QUIT, nil, nil, 'Quit', Proc.new { |g,a| do_quit }],
				
				['EditMenuAction', nil, '_Edit', nil, nil, nil],
				['PasteAction', Gtk::Stock::PASTE, nil, nil, 'Paste objects', Proc.new { |g,a| do_paste }],
				['GroupAction', nil, 'Group', '<Control>g', 'Group objects', Proc.new { |g,a| do_group }],
				['UngroupAction', nil, 'Ungroup', '<Control>u', 'Ungroup object', Proc.new { |g,a| do_ungroup }],
			
				['AddStateAction', Stock::STATE, nil, nil, 'Add state', Proc.new { |g,a| @grid << Components::State }],
				['AddLinkAction', Stock::LINK, nil, nil, 'Link objects', Proc.new { |g,a| @grid << Components::Link }],
				['ViewMenuAction', nil, '_View', nil, nil, nil],
				['CenterAction', Gtk::Stock::JUSTIFY_CENTER, nil, '<Control>h', 'Center view', Proc.new { |g,a| do_center_view }],
				['InsertMenuAction', nil, '_Insert', nil, nil, nil],
				
				['MonitorMenuAction', nil, 'Monitor', nil, nil, nil],
				['ControlMenuAction', nil, 'Control', nil, nil, nil],
				['PropertiesAction', nil, 'Properties', nil, nil, Proc.new { |g,a| do_object_activated(@grid.selection.first) }],
				['EditGroupAction', nil, 'Edit group', nil, nil, Proc.new { |g,a| @grid.level_down(@grid.selection.first) }]
			])
			
			ag.add_toggle_actions([
				['PropertyEditorAction', Gtk::Stock::PROPERTIES, 'Property Editor', '<Control>F9' ,'Show/Hide property editor pane', Proc.new { |g,a| do_property_editor(a) }, true],
				['ViewMonitorAction', nil, 'Monitor', '<Control>m', 'Show/Hide monitor window', Proc.new { |g,a| toggle_monitor(a.active?) }, false],
				['ViewControlAction', nil, 'Control', '<Control>k', 'Show/Hide control window', Proc.new { |g,a| toggle_control(a.active?) }, false],
			])

			@uimanager.insert_action_group(ag, 0)
			@normal_group = ag
			
			ag = Gtk::ActionGroup.new('SelectionActions')
			ag.add_actions([
				['CutAction', Gtk::Stock::CUT, nil, nil, 'Cut objects', Proc.new { |g,a| do_cut }],
				['CopyAction', Gtk::Stock::COPY, nil, nil, 'Copy objects', Proc.new { |g,a| do_copy }],
				['DeleteAction', Gtk::Stock::DELETE, nil, nil, 'Delete object', Proc.new { |g,a| do_delete }]
			])
			
			@uimanager.insert_action_group(ag, 0)
			@selection_group = ag
			
			@uimanager.add_ui(File.join(File.dirname(__FILE__), 'ui.xml'))
			
			build_templates(@uimanager)
			
			add_accel_group(@uimanager.accel_group)
		
			vbox.pack_start(@uimanager.get_widget('/menubar'), false, false, 0)
			vbox.pack_start(toolbar = @uimanager.get_widget('/toolbar'), false, false, 0)
		
			@path_box = Gtk::HBox.new(false, 3)
			vbox.pack_start(@path_box, false, false, 0)
			
			@vboxcontents = Gtk::VBox.new(false, 3)
			vbox.pack_start(@vboxcontents, true, true, 0)
		
			@grid = Grid.new
			@simulation.root = @grid.root

			@vpaned = Gtk::VPaned.new
			@vpaned.position = 0
			@vpaned.pack1(@grid, true, true)
			
			do_property_editor(@normal_group.get_action('PropertyEditorAction'))
			
			@vboxcontents.pack_start(@vpaned, true, true, 0)

			hbox = Gtk::HBox.new(false, 6)
			
			pentry = Gtk::Entry.new
			pentry.set_size_request(75, -1)
			@periodentry = pentry

			pentry.signal_connect('activate') do |b|
				do_simulation_period(pentry)
			end		
			
			but = Gtk::Button.new
			but.image = Gtk::Image.new(Gtk::Stock::MEDIA_FORWARD, Gtk::IconSize::BUTTON)
			but.label = 'Simulate period'
			but.signal_connect('clicked') { |but| do_simulation_period(pentry) }
			hbox.pack_start(Gtk::Label.new('Period:'), false, false, 0)
			hbox.pack_start(pentry, false, false, 0)
			hbox.pack_start(Gtk::Label.new('(s)'), false, false, 0)
			hbox.pack_start(but, false, false, 0)
		
			but = Gtk::Button.new
			but.image = Gtk::Image.new(Gtk::Stock::MEDIA_NEXT, Gtk::IconSize::BUTTON)
			but.label = 'Step'
			but.signal_connect('clicked') { |but| do_simulation_step_it(but) }
			hbox.pack_end(but, false, false, 0)

			but = Gtk::ToggleButton.new
			but.image = Gtk::Image.new(Gtk::Stock::MEDIA_PLAY, Gtk::IconSize::BUTTON)
			but.label = 'Simulate'
			but.signal_connect('toggled') { |but| do_simulation(but) }
			hbox.pack_end(but, false, false, 0)
		
			@simulate_button = but
		
			but = Gtk::Button.new
			but.image = Gtk::Image.new(Gtk::Stock::CLEAR, Gtk::IconSize::BUTTON)
			but.label = 'Reset'
			but.signal_connect('clicked') { |but| do_simulation_reset }
			hbox.pack_end(but, false, false, 0)
		
			@vboxcontents.pack_start(hbox, false, false, 0)
			
			@statusbar = Gtk::Statusbar.new
			@statusbar.show
			vbox.pack_start(@statusbar, false, false, 3)

			add(vbox)
		
			@grid.signal_connect('activated') do |grid, obj|
				do_object_activated(obj)
			end
		
			@grid.signal_connect('popup') do |grid, button, time|
				do_popup(button, time)
			end
		
			@grid.signal_connect('object_removed') do |grid, obj|
				if @property_editors.include?(obj)
					@property_editors[obj].destroy
				end
			end
		
			@grid.signal_connect('level_down') do |grid, obj|
				handle_level_down(obj)
			end
		
			@grid.signal_connect('level_up') do |grid, obj|
				handle_level_up(obj)
			end
			
			@grid.signal_connect('modified') do |grid|
				if !@modified
					@modified = true
					update_title
				end
			end
			
			@grid.signal_connect('selection_changed') do |grid|
				if @propertyview
					if @grid.selection.length == 1
						@propertyview.init(@grid.selection.first)
					else
						@propertyview.init(nil)
					end
				end
				
				update_sensitivity
			end
			
			@grid.signal_connect('focus-out-event') do |grid, event|
				update_sensitivity
			end
			
			@grid.signal_connect('focus-in-event') do |grid, event|
				update_sensitivity
			end
		end
		
		def update_sensitivity
			objs = @grid.selection
			
			@selection_group.sensitive = !objs.empty? && @grid.has_focus?
			
			singleobj = objs.length == 1
			singlegroup = (singleobj && objs[0].is_a?(Components::Group))
			anygroup = objs.select { |x| x.is_a?(Components::Group) }.length

			ungroup = @normal_group.get_action('UngroupAction')
			ungroup.sensitive = anygroup > 0
			
			if anygroup > 1
				ungroup.label = 'Ungroup all'
			else
				ungroup.label = 'Ungroup'
			end
			
			@normal_group.get_action('GroupAction').sensitive = (objs.length > 1 && objs.find { |x| not x.is_a?(Components::Attachment) })
			@normal_group.get_action('EditGroupAction').sensitive = singlegroup
			
			['Properties'].each do |x|
				@normal_group.get_action("#{x}Action").sensitive = singleobj
			end
			
			@normal_group.get_action('PasteAction').sensitive = @grid.has_focus? 
		end
	
		def run
			if !ARGV.empty?
				do_load_xml(File.expand_path(ARGV[0]))
			end
		
			Gtk::main
		end
		
		def toggle_monitor(active)
			if !active && @monitor
				ctrl = @monitor
				@monitor = nil
				ctrl.destroy
			elsif active
				ensure_monitor
				@monitor.present
			end
		end
		
		def toggle_control(active)
			if !active && @control
				ctrl = @control
				@control = nil
				ctrl.destroy
			elsif active
				ensure_control
				@control.present
			end
		end
	
		def signal_do_delete_event(event)
			ask_unsaved_modified

			GLib::Source.remove(@simulation_source) if @simulation_source
			Gtk::main_quit
		end
	
		def do_object_activated(object)
			if @property_editors.include?(object)
				@property_editors[object].present
				return
			end
		
			dlg = PropertyEditor.new(self, object)
			position_window(dlg)
			dlg.show

			@property_editors[object] = dlg
		
			dlg.signal_connect('response') do |dlg, res|
				@grid.queue_draw
				@property_editors.delete(object)
				dlg.destroy
			end
		end
		
		def do_revert
			fname = @filename
			clear
			@modified = false
			
			do_load_xml(fname)
		end
	
		def do_quit
			Gtk::main_quit
		end
	
		def do_new
			clear
			
			@filename = nil
			@modified = false
			update_title
		end
	
		def do_center_view
			@grid.center_view
		end
	
		def push_path(obj)
			but = Gtk::Button.new(obj ? obj.to_s : '(cpg)')
			but.relief = Gtk::ReliefStyle::NONE
			but.show
		
			@path_box.pack_start(but, false, false, 0)
		
			if block_given?
				but.signal_connect('clicked') { |but| yield but }
			end
		
			if obj
				sid = obj.signal_connect('property_changed') do |o, prop|
					but.label = o.to_s
				end
			
				but.signal_connect('destroy') do |b|
					obj.signal_handler_disconnect(sid)
				end
			end
		end
	
		def pop_path
			@path_box.remove(@path_box.children[-1])
		end
	
		def clear
			@grid.clear
			@simulation.root = @grid.root
		
			while @path_box.children.length > 0
				@path_box.remove(@path_box.children.first)
			end
		
			push_path(nil) { |but| @grid.level_up(@grid.root) }
			
			@grid.grab_focus
		end
		
		def ask_unsaved_modified
			if @modified
				dlg = Gtk::MessageDialog.new(self, 
										     Gtk::Dialog::DESTROY_WITH_PARENT | Gtk::Dialog::MODAL, 
										     Gtk::MessageDialog::WARNING,
										     Gtk::MessageDialog::BUTTONS_YES_NO,
										     'There are unsaved changes in the current network, do you want to save these changes first?')
				
				res = dlg.run
				
				if res == Gtk::Dialog::RESPONSE_YES
					d = do_save
					
					d.run if d
				end	
				
				dlg.destroy			     
			end
		end
	
		def do_load_xml(filename)
			ask_unsaved_modified

			begin
				cpg = Loader.load(File.new(filename, 'r'))
			rescue Exception
				show_message(Gtk::Stock::DIALOG_ERROR, "<b>Could not load CPG file #{filename}</b>", "<i>Please make sure that this is a valid CPG file</i>")
				STDERR.puts("Could not load cpg file #{filename}: #{$!}\n\t#{$@.join("\n\t")}")
				return
			end

			# clear everything
			clear
					
			@filename = filename
			map = {}

			resize(cpg.allocation.width ? cpg.allocation.width.to_i : allocation.width, cpg.allocation.height ? cpg.allocation.height.to_i : allocation.width)
			
			while Gtk.events_pending?
				# make sure to push resize through
				Gtk.main_iteration
			end
		
			if cpg.allocation.x
				@grid.root.x = cpg.allocation.x.to_i
			end
		
			if cpg.allocation.y
				@grid.root.y = cpg.allocation.y.to_i
			end
		
			if cpg.pane_position
				@vpaned.position = @vpaned.allocation.height - cpg.pane_position.to_i
			end
			
			if cpg.zoom
				@grid.grid_size = cpg.zoom.to_f
			end

			cpg.each do |obj|
				map[obj.get_property('id')] = obj if obj.properties.include?('id')
				@grid.add(obj, obj.allocation.x, obj.allocation.y, obj.allocation.width, obj.allocation.height)
			end
			
			if cpg.period
				@periodentry.text = cpg.period.to_s
			end
			
			if cpg.monitors && !cpg.monitors.empty?
				ensure_monitor

				if cpg.monitors_size && cpg.monitors_size.length == 2
					s = cpg.monitors_size
					@monitor.set_size(s[0], s[1])
				end

				cpg.monitors.each do |monitor|
					next unless map[monitor.id]

					@monitor.add_hook(map[monitor.id], monitor.name)
					@monitor.set_yaxis(map[monitor.id], monitor.name, [monitor.ymin.to_f, monitor.ymax.to_f])
				end
			end
			
			if cpg.controls && !cpg.controls.empty?
				ensure_control
				
				if cpg.controls_size && cpg.controls_size.length == 2
					s = cpg.controls_size
					@control.set_size(s[0], s[1])
				end

				cpg.controls.each do |control|
					@control.add_hook(map[control.id], control.name) if map[control.id]
				end
			end
		
			@modified = false
			update_title
			@grid.queue_draw
			
			status_message("Loaded #{@filename} ...")
		end
	
		def do_open
			dlg = Gtk::FileChooserDialog.new('Open CPG file', 
							 self, 
							 Gtk::FileChooser::ACTION_OPEN,
							 nil,
							 [Gtk::Stock::CANCEL, Gtk::Dialog::RESPONSE_CANCEL],
							 [Gtk::Stock::OPEN, Gtk::Dialog::RESPONSE_ACCEPT])
		
			dlg.signal_connect('response') do |dlg, resp|
				filename = dlg.filename
				dlg.destroy
				
				if resp == Gtk::Dialog::RESPONSE_ACCEPT
					do_load_xml(filename)
				end
			end
		
			dlg.show
		end
		
		def update_title
			extra = @modified ? '*' : ''
			
			if @filename
				self.title = "#{extra}#{File.basename(@filename)} - CPG Studio"
			else
				self.title = "#{extra}New Network - CPG Studio"
			end
		end
	
		def do_save_xml
			id = 0
		
			# fill in reference ids if necessary
			Saver.ensure_ids(@grid.root)
		
			objects = @grid.root_objects
			objects.delete_if { |x| !x.is_a?(Serialize::Object) }

			cpg = Cpg.new(objects)
			cpg.allocation = Allocation.new([@grid.root.x.to_i, @grid.root.y.to_i, allocation.width, allocation.height])
			
			cpg.pane_position = @vpaned.allocation.height - @vpaned.position
			cpg.zoom = @grid.root_grid_size
			cpg.period = @periodentry.text
			
			# set monitors and controls
			if @monitor && @monitor.visible?
				@monitor.each_hook do |obj, prop|
					next unless obj.properties.include?('id')

					yax = @monitor.yaxis(obj, prop)

					cpg.monitors << Serialize::Monitor.new.merge!({
						'id' => obj.get_property('id').to_s,
						'name' => prop.to_s,
						'ymin' => yax[0],
						'ymax' => yax[1]})
				end
				
				cpg.monitors_size = Serialize::Array.new(@monitor.size)
			end
			
			if @control && @control.visible?
				@control.each_hook do |obj, prop|
					next unless obj.properties.include?('id')

					cpg.controls["#{obj.get_property('id')}"] = prop.to_s
				end
				
				cpg.controls_size = Serialize::Array.new(@control.size)
			end
			
			doc = Saver.save(cpg)
		
			begin
				f = File.new(@filename, 'w')
				doc.write(f, 2)
				f.puts ''
				f.close
				
				status_message("Saved #{@filename} ...")
				@modified = false
				update_title
			rescue Exception
				show_message(Gtk::Stock::DIALOG_ERROR, "<b>Could not save CPG file #{@filename}</b>", "<i>Do you have the proper permissions to write in that location?</i>")
				STDERR.puts("Could not save cpg file #{@filename}: #{$!}\n\t#{$@.join("\n\t")}")
			end
		end
		
		def export_to_flat(objs)
			objs.delete_if { |x| !(x.is_a?(Components::State) || x.is_a?(Components::Link) || x.is_a?(Components::Group)) }
			objs = normalize_objects(objs)
			
			return FlatFormat.format(objs)
		end
		
		def do_save_flat(filename)
			Saver.ensure_ids(@grid.root)
			
			s = export_to_flat(@grid.root_objects.dup)
		
			f = File.new(filename, 'w')
			f.write(s)
			f.puts ''
			f.close
			
			status_message("Saved flat file to #{filename}...")
		end
		
		def add_save_filters(dlg)
			autofilter = Gtk::FileFilter.new
			autofilter.add_pattern('*.cpg')
			autofilter.add_pattern('*.xml')
			autofilter.add_pattern('*.txt')
			autofilter.name = 'Detect automatically'
			
			dlg.add_filter(autofilter)
			
			xmlfilter = Gtk::FileFilter.new
			xmlfilter.add_pattern('*.cpg')
			xmlfilter.add_pattern('*.xml')
			xmlfilter.name = 'CPG Network File (*.cpg)'

			dlg.add_filter(xmlfilter)
			
			flatfilter = Gtk::FileFilter.new
			flatfilter.add_pattern('*.txt')
			flatfilter.name = 'Flat Format File (*.txt)'
			dlg.add_filter(flatfilter)
			
			{:auto => autofilter, :xml => xmlfilter, :flat => flatfilter}
		end
		
		def do_save_as
			dlg = Gtk::FileChooserDialog.new('Save CPG file', 
							 self, 
							 Gtk::FileChooser::ACTION_SAVE,
							 nil,
							 [Gtk::Stock::CANCEL, Gtk::Dialog::RESPONSE_CANCEL],
							 [Gtk::Stock::SAVE, Gtk::Dialog::RESPONSE_ACCEPT])
		
			dlg.current_folder = File.dirname(@filename) if @filename
			filters = add_save_filters(dlg)
		
			dlg.signal_connect('response') do |dlg, resp|
				if resp == Gtk::Dialog::RESPONSE_ACCEPT
					if dlg.filter == filters[:xml] || (dlg.filter == filters[:auto] && !(dlg.filename =~ /\.txt$/))
						@filename = dlg.filename
						do_save_xml
					else
						do_save_flat(dlg.filename)
					end
				end
			
				dlg.destroy
			end
		
			dlg.show
			dlg
		end
	
		def do_save
			if @filename != nil
				do_save_xml
				return
			end
		
			do_save_as
		end
		
		def import_attachments(attachments)
			attachments.each do |at|
				@grid << at
			end
		end

		def import_from_xml(text)
			cpg = Loader.load(text)
			raise('Invalid CPG file') unless (cpg and cpg.is_a?(Cpg))
			
			# test if there are only attachments
			if not cpg.find { |x| not x.is_a?(Components::Attachment) }
				import_attachments(cpg)
				return
			end
			
			# remove any rogue attachments (those which do not have the minimum
			# amount of objects)
			objs = cpg.dup
			objs.delete_if do |x|
				ret = x.is_a?(Components::Attachment) && x.objects.compact.length < x.class.limits[0]
			end
		
			# for each of the objects, put them on the grid, make sure ids
			# differ and normalize the position (center it)
			dx, dy = @grid.mean_position(cpg)
			cx = (@grid.current.x + (@grid.allocation.width / 2.0)) / @grid.grid_size.to_f
			cy = (@grid.current.y + (@grid.allocation.height / 2.0)) / @grid.grid_size.to_f
			
			cpg.each do |obj|
				if obj.properties.include?('id')
					@grid.ensure_unique_id(obj, obj.get_property('id'))
				end
			
				if !obj.is_a?(Components::Attachment)
					obj.allocation.x = (cx + ((obj.allocation.x || 0) - dx) + 0.00001).round.to_i
					obj.allocation.y = (cy + ((obj.allocation.y || 0) - dy) + 0.00001).round.to_i
				end
			
				@grid.add(obj, obj.allocation.x, obj.allocation.y, obj.allocation.width, obj.allocation.height)
			end	
		end
		
		def import_from_file(filename)
			begin
				import_from_xml(File.new(filename, 'r').read)
				
				status_message("Imported #{@filename} ...")
			rescue Exception
				show_message(Gtk::Stock::DIALOG_ERROR, "<b>Error while importing from #{filename}</b>", "<i>#{$!}</i>")
				STDERR.puts("Error in import: #{$!}\n\t#{$@.join("\n\t")}")
				false
			end
		end
		
		def do_import
			dlg = Gtk::FileChooserDialog.new('Import CPG file', 
							 self, 
							 Gtk::FileChooser::ACTION_OPEN,
							 nil,
							 [Gtk::Stock::CANCEL, Gtk::Dialog::RESPONSE_CANCEL],
							 [Gtk::Stock::OPEN, Gtk::Dialog::RESPONSE_ACCEPT])
		
			dlg.signal_connect('response') do |dlg, resp|
				if resp == Gtk::Dialog::RESPONSE_ACCEPT
					import_from_file(dlg.filename)
				end
			
				dlg.destroy
			end
		
			dlg.show
		end
		
		def normalize_objects(objs)
			if objs.find { |x| not x.is_a?(Components::Attachment) }
				remove, objs = @grid.normalize(objs)
				objs
			else
				objs.dup
			end
		end
		
		def do_export
			dlg = Gtk::FileChooserDialog.new('Export CPG file', 
							 self, 
							 Gtk::FileChooser::ACTION_SAVE,
							 nil,
							 [Gtk::Stock::CANCEL, Gtk::Dialog::RESPONSE_CANCEL],
							 [Gtk::Stock::OPEN, Gtk::Dialog::RESPONSE_ACCEPT])
		
			dlg.current_folder = File.dirname(@filename) if @filename
			filters = add_save_filters(dlg)

			if !@grid.selection.empty?
				objs = @grid.selection.dup
			else
				objs = @grid.current.children.dup
			end

			xml = export_to_xml(objs)
			flat = export_to_flat(objs)
							
			dlg.signal_connect('response') do |dlg, resp|
				if resp == Gtk::Dialog::RESPONSE_ACCEPT
					begin
						f = File.new(dlg.filename, 'w')
						
						if dlg.filter == filters[:xml] || (dlg.filter == filters[:auto] && !(dlg.filename =~ /\.txt$/))
							f.puts(xml)
						else
							f.puts(flat)
						end
						
						f.close
						status_message("Exported to #{dlg.filename} ...")
					rescue Exception
						show_message(Gtk::Stock::DIALOG_ERROR, "<b>Error while exporting to #{dlg.filename}</b>", "<i>#{$!}</i>")
					end
				end
			
				dlg.destroy
			end
		
			dlg.show
		end
		
		def do_delete
			@grid.delete_objects
		end
		
		def do_simulation_period(entry)
			if entry.text =~ /[:,]/
				c = MathContext.new(Simulation.instance.state)
				s = Range.new(entry.text)
				
				from = c.eval(s.from).to_f
				step = s.explicitstep ? c.eval(s.step).to_f : Simulation.instance.timestep
				to = c.eval(s.to).to_f
			else
				from = 0
				to = entry.text.to_f
				step = Simulation.instance.timestep
			end

			if step <= 0 || from >= to
				entry.modify_base(Gtk::STATE_NORMAL, Gdk::Color.parse('#FF6666'))
				entry.modify_text(Gtk::STATE_NORMAL, Gdk::Color.parse('white'))
				return
			end
			
			style = entry.modifier_style
			style.set_color_flags((Gtk::STATE_NORMAL).to_i, style.color_flags(Gtk::STATE_NORMAL) & (~Gtk::RC::BASE & ~Gtk::RC::TEXT))
			entry.modify_style(style)
			
			# stop running simulation
			@simulate_button.active = false if @simulation.running?
			
			# reset simulation
			@simulation.reset
			
			# start simulate period
			t = Time.now
			@simulation.simulate_period(from, step, to)
			
			status_message("Simulation finished in #{format('%.2f', Time.now - t)}s")
			
			# check if any of the values in any of the objects are invalid
			if !check_object_values(@grid.current)
				show_message(Gtk::Stock::DIALOG_ERROR, '<b>Unstable simulation</b>', '<i>NaN or Inifinite values were detected after the simulation. This generally means that the simulation was not numerically stable.</i>')
			end
		end
		
		def check_object_values(o)
			o.properties.each do |p, v|
				if v.is_a?(Float) && (v.nan? || v.infinite?)
					return false
				end
			end
			
			if o.is_a?(Components::Group)
				o.children.each do |x|
					return false if !check_object_values(x)
				end
			end
			
			true
		end
	
		def do_simulation_reset
			# reset states
			@simulation.reset
		end
	
		def do_simulation_step_it(button)
			@simulation.stop if @simulation.running?
			@simulation.step

			@simulate_button.active = false if @simulate_button.active?
		end
	
		def do_simulation(button)
			@simulation_paused = false

			if !button.active?
				button.image = Gtk::Image.new(Gtk::Stock::MEDIA_PLAY, Gtk::IconSize::BUTTON)
				
				@simulation.stop
			else
				button.image = Gtk::Image.new(Gtk::Stock::MEDIA_PAUSE, Gtk::IconSize::BUTTON)
			
				# run simulation
				@simulation.start
			end
		end
		
		def position_window(w)
			x, y = self.position
			
			sw = self.screen.width
			sh = self.screen.height
			
			mw, mh = self.size
			ww, wh = w.size
			
			mw += 10
			mh += 10
			ww += 10
			wh += 10
			
			# see where the most room is
			maxx = [x, sw - (x + mw)].max
			maxy = [y, sh - (y + mh)].max
			
			if maxx > ww && maxx > maxy
				nx = x > sw - (x + mw) ? x - ww : x + mw
				ny = y
			elsif maxy > wh && maxy > maxx
				nx = x
				ny = y > sh - (y + mh) ? y - wh : y + mh
			else
				nx = sw - ww
				ny = sh - wh
			end
			
			# push window position
			w.move([[nx, sw - ww].min, 0].max, [[ny, sh - wh].min, 0].max)
		end
	
		def ensure_monitor
			if not @monitor
				@monitor = Monitor.new(@simulation.timestep)
				@monitor.set_transient_for(self)
				
				@monitor.realize
				self.present
				Gtk.main_iteration while Gtk.events_pending?
				position_window(@monitor)
				
				@monitor.present
				Gtk.main_iteration while Gtk.events_pending?

				position_window(@monitor)

				@monitor.signal_connect('destroy') do |w| 
					@monitor = nil
					@normal_group.get_action('ViewMonitorAction').active = false
				end
			end
		end
	
		def ensure_control
			if not @control
				@control = Control.new
				@control.set_transient_for(self)

				self.present
				@control.realize				
				position_window(@control)
				
				@control.present
				position_window(@control)
			
				@control.signal_connect('destroy') do |w| 
					@control = nil
					@normal_group.get_action('ViewControlAction').active = false
				end
				
				@control.signal_connect('pause_simulation') do |w|
					if @simulation.running?
						@simulate_button.active = false
						@simulation_paused = true
					end
				end
			
				@control.signal_connect('continue_simulation') do |w|
					if @simulation_paused
						@simulate_button.active = true
						@simulation_paused = false
					end
				end
			end
		end
	
		def start_monitor(obj, prop)
			ensure_monitor
		
			(obj.is_a?(Enumerable) ? obj : [obj]).each do |o|
				@monitor.add_hook(o, prop)
			end
		end
	
		def start_control(obj, prop)
			ensure_control

			(obj.is_a?(Enumerable) ? obj : [obj]).each do |o|
				@control.add_hook(o, prop)
			end
		end
	
		def do_group
			if @grid.selection.length > 1
				@grid.group(@grid.selection.dup)
			end
		end
	
		def do_ungroup
			# find all groups in the selection
			objs = @grid.selection.select { |x| x.is_a?(Components::Group) }

			# for each group, ungroup it			
			objs.each do |obj|
				@grid.ungroup(obj)
			end
		end
		
		def common_properties(objs)
			sets = objs.collect { |x| x.properties.keys.select { |p| !x.invisible?(p) }.to_set }
			
			# intersect on all sets
			sets.inject { |v, a| v & a }
		end
	
		def do_popup(button, time)
			if @popupmergeid
				@uimanager.remove_ui(@popupmergeid)
				@uimanager.remove_action_group(@popupactiongroup)
			end
			
			@popupmergeid = @uimanager.new_merge_id
			@popupactiongroup = Gtk::ActionGroup.new('PopupDynamic')
			@uimanager.insert_action_group(@popupactiongroup, 0)
			
			# merge monitor and control
			objs = @grid.selection.dup
			props = common_properties(objs)

			if !props.empty?
				['Monitor', 'Control'].each do |type|
					props.each do |prop|
						next if prop == 'id'
						name = "#{type}#{prop}"
						
						@popupactiongroup.add_actions([
							["#{name}Action", nil, prop.to_s, nil, nil, Proc.new { |g,a| send("start_#{type.downcase}", objs, prop) }]
						])
						
						@uimanager.add_ui(@popupmergeid, "/popup/#{type}Menu/#{type}Placeholder", name, "#{name}Action", Gtk::UIManager::MENUITEM, false) 
					end
				end
			end
			
			menu = @uimanager.get_widget('/popup')
			menu.show_all
			menu.popup(nil, nil, button, time)
		end
	
		def export_to_xml(objs)
			# filter objs to remove attachments for which either the destination
			# or the source is not present
			objs = normalize_objects(objs)
			doc = Saver.save(Cpg.new(objs))
		
			o = ''
			doc.write(REXML::Output.new(o, 'utf-8'), 2)
		
			return o
		end
	
		def export_to_clipboard(clipboard, selection_data, xml)
			selection_data.text = xml
			true
		end
	
		def do_cut
			return if @grid.selection.empty?
		
			clip = Gtk::Clipboard.get(Gdk::Selection::CLIPBOARD)
			
			xml = export_to_xml(@grid.selection)
			@clipboard = xml
			
			# use only internal clipboard for now seems bindings seem to be
			# buggy

			#clip.set(CutAndPaste::TARGETS) do |clipboard, selection_data|
			#	export_to_clipboard(clipboard, selection_data, xml)
			#end
		
			do_delete
		end
	
		def do_copy
			return if @grid.selection.empty?
		
			clip = Gtk::Clipboard.get(Gdk::Selection::CLIPBOARD)
			
			xml = export_to_xml(@grid.selection)
			@clipboard = xml
			
			# use only internal clipboard for now seems bindings seem to be
			# buggy
			
			#clip.set(CutAndPaste::TARGETS) do |clipboard, selection_data|
			#	export_to_clipboard(clipboard, selection_data, xml)
			#end
		end
	
		def handle_paste_request(text)
			return false unless text
		
			begin
				import_from_xml(text)
			rescue Exception
				STDERR.puts("Error in import: #{$!}\n\t#{$@.join("\n\t")}")
				false
			end
		end
		
		def status_message(msg)
			@statusbar.push(0, msg)
			
			GLib::Source.remove(@statustimeout) if @statustimeout
			
			@statustimeout = GLib::Timeout.add(3000) do
				@statusbar.push(0, '')
				@statustimeout = nil
				false
			end
		end
		
		def show_message(icon, primary, secondary = nil, actions = nil)
			contents = Gtk::HBox.new(false, 6)
			
			if icon
				contents.pack_start(icon.is_a?(Gtk::Widget) ? icon : Gtk::Image.new(icon, Gtk::IconSize::DIALOG), false, false, 0)
			end
			
			vbox = Gtk::VBox.new(false, 12)
			lbl = Gtk::Label.new
			lbl.xalign = 0
			lbl.yalign = 0
			lbl.markup = primary
			
			vbox.pack_start(lbl, false, true, 0)
			
			if secondary
				lbl = Gtk::Label.new
				lbl.markup = secondary
				lbl.xalign = 0
				lbl.yalign = 0
				vbox.pack_start(lbl, false, true, 0)
			end
			
			contents.pack_start(vbox, true, true, 0)
			
			if not @messagearea
				@messagearea = MessageArea.new
				@vboxcontents.pack_start(@messagearea, false, false, 0)
				@vboxcontents.reorder_child(@messagearea, 0)
				
				@messagearea.signal_connect('destroy') do |dlg|
					@messagearea = nil
				end
			end
			
			if actions == nil
				actions = [
					[Gtk::Button.new(Gtk::Stock::CLOSE), Proc.new { @messagearea.destroy }]
				]
			end
			
			@messagearea.contents = contents
			@messagearea.actions = actions
			@messagearea.show_all
		end
	
		def do_paste
			# use internal clipboard for now since there seems to be a severe
			# problem with the ruby gnome2 bindings for the clipboard
			handle_paste_request(@clipboard) if @clipboard
			#clipboard = Gtk::Clipboard.get(Gdk::Selection::CLIPBOARD)

			#begin
			#	clipboard.request_text do |clip, text|
			#		handle_paste_request(text)
			#		true
			#	end
			#rescue Exception
			#	STDERR.puts("Could not load cpg from text: #{$!}\n\t#{$@.join("\n\t")}")
			#	return false
			#end
		end
	
		def handle_level_up(obj)
			pop_path
			
			@simulation.root = @grid.current
		end
	
		def handle_level_down(obj)
			push_path(obj) { |but| @grid.level_up(obj) }
			
			@simulation.root = @grid.current
		end
		
		def do_property_editor(a)
			if a.active? && @propertyview == nil
				@propertyview = PropertyView.new(@grid.selection.length == 1 ? @grid.selection[0] : nil)
				@vpaned.pack2(@propertyview, false, false)
				@propertyview.show_all
			elsif !a.active? && @propertyview
				@propertyview.destroy
				@propertyview = nil
			end
		end
		
		def signal_do_configure_event(ev)
			super
			
			if @vpaned.position == 0
				@vpaned.position = (@vpaned.allocation.height * 0.7).to_i
			end
		end
	end
end
