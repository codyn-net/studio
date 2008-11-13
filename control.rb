require 'gtk2'
require 'hook'
require 'table'

module Cpg
	class Control < Hook
		type_register
	
		signal_new('pause_simulation',
			GLib::Signal::RUN_LAST,
			nil,
			nil)
	
		signal_new('continue_simulation',
			GLib::Signal::RUN_LAST,
			nil,
			nil)
	
		def initialize()
			super({:title => 'Control'})
		
			@grid = Application.instance.grid
			@linked = []
		
			set_default_size(500, 200)
			show_all
		end
		
		def content_area
			@content = Table.new(1, 1, true)
			@content.expand = Table::EXPAND_RIGHT
			@content.row_spacings = 1
			@content.column_spacings = 1
			
			@content
		end
		
		def control_position(obj, prop)
			h = find_hook(obj, prop)
			
			return unless h
			@content.get_position(h[:widget])
		end
		
		def set_control_position(obj, prop, x, y)
			h = find_hook(obj, prop)
			
			return unless h
			@content.child_position(h[:widget], x, y)
		end
		
		def set_size(cols, rows)
			cols = cols.to_i
			rows = rows.to_i
			
			cols = @content.n_columns if cols <= 0
			rows = @content.n_rows if rows <= 0
			
			@content.resize(rows, cols)
		end
		
		def size
			return [@content.n_columns, @content.n_rows]
		end
	
		def center_label(s)
			lbl = Gtk::Label.new(s)
		
			lbl.justify = Gtk::Justification::CENTER
			lbl
		end
		
		def get_range(obj, prop)
			c = MathContext.new(Simulation.instance.state, obj.state)
			range = obj.get_range(prop)

			if !range
				v = obj.initial_value(prop)
				v = obj.get_property(prop) unless (v && !v.to_s.empty?)
				v = c.eval(v).to_f
				range = Range.new("#{v - 10 * (v.abs + 1)}:#{v + 10 * (v.abs + 1)}")
			end
			
			from = c.eval(range.from).to_f
			step = c.eval(range.step).to_f
			to = c.eval(range.to).to_f
			
			return [from, step, to]
		end
		
		def act_linked(mine, val)
			@linked.each do |x|
				x.value = val unless x == mine
			end
		end

		def make_slider(obj, prop, val, isinit)
			from, step, to = get_range(obj, prop)

			actuator = Gtk::VScale.new(from, to, step)
			actuator.value = val
			actuator.inverted = true
			actuator.show_fill_level = true
			actuator.fill_level = actuator.value
			actuator.restrict_to_fill_level = false
			actuator.value_pos = isinit ? Gtk::PositionType::RIGHT : Gtk::PositionType::LEFT
	
			actuator.signal_connect('value_changed') do |s|
				s.fill_level = s.value

				if !@updating || !(@updating[0] == obj && @updating[1] == prop && @updating[2] == isinit)
					if !isinit
						if obj.is_a?(Components::SimulatedObject) && obj.integrated?(prop)
							signal_emit('pause_simulation')
						end
		
						obj.set_property(prop, s.value)
					else
						obj.set_initial_value(prop, s.value)
					end
					
					act_linked(s, s.value)
				end
			end
	
			if obj.is_a?(Components::SimulatedObject) && obj.integrated?(prop) && !isinit
				actuator.signal_connect('button_release_event') do |s, ev|
					signal_emit('continue_simulation')
				end
			end
			
			actuator
		end
		
		def update_initial(obj, container)
			oldu = @updating
			@updating = [obj, container[:prop]]
			
			c = Simulation.instance.setup_context(obj)
			container[:act2].value = c.eval(obj.initial_value(container[:prop])).to_f
			
			@updating = oldu
		end
		
		def update_range(obj, container)
			prop = container[:prop]
			
			oldu = @updating
			@updating = [obj, prop]

			from, step, to = get_range(obj, prop)
			
			container[:from_lbl].text = format('%.3f', from)
			container[:to_lbl].text = format('%.3f', to)

			[container[:act1], container[:act2]].compact.each do |act|
				act.set_range(from, to)
				act.set_increments(step, step * 10)
			end
			
			@updating = oldu
		end
		
		def install_object(obj, prop)
			super
			
			connect(obj, 'property_changed') do |o, p|
				@map[obj].each do |x|
					if ['display', 'id', 'equation', 'label'].include?(p)
						t = property_name(obj, x[:prop])
						x[:frame].label = t
						
						x[:frame].set_tooltip_text(property_name(obj, x[:prop], true))
					end
					 
					next if x[:prop] != p
				
					c = MathContext.new(Simulation.instance.state, o.state)
			
					@updating = [obj, p]
					act = x[:act1]
					val = c.eval(o.get_property(p)).to_f

					act.value = [[act.adjustment.lower, val].max, act.adjustment.upper].min
					act.fill_level = act.value
				
					@updating = nil
				end
			end

			connect(obj, 'range_changed') do |o, p|
				o = @map[obj].find { |x| x[:prop] == p }
				update_range(obj, o) if o
			end
		
			connect(obj, 'initial_changed') do |o, p|
				o = @map[obj].find { |x| x[:prop] == p }
				update_initial(obj, o) if o
			end
		end
		
		def remove_hook_real(obj, container)
			@linked.delete(container[:act1])
			@linked.delete(container[:act2])
		end
	
		def add_hook_real(obj, prop, state)
			frame = Gtk::Frame.new(property_name(obj, prop))
			frame.set_tooltip_text(property_name(obj, prop, true))
			
			vbox = Gtk::VBox.new(false, 3)
			
			frame << vbox
			
			hbox = Gtk::HBox.new(false, 6)	

			close = Gtk::Button.new
			close.image = Gtk::Image.new(Gtk::Stock::CLOSE, Gtk::IconSize::MENU)
			close.relief = Gtk::ReliefStyle::NONE
		
			close.signal_connect('clicked') { |x| remove_hook(obj, prop) }
			hbox.pack_end(close, false, false, 0)
			
			vbox.pack_start(hbox, false, true, 0)
			vbox.pack_start(to_lbl = center_label(''), false, true, 0)
			
			c = Simulation.instance.setup_context(obj)
			
			hboth = Gtk::HBox.new(false, 3)
			s1 = make_slider(obj, prop, c.eval(obj.get_property(prop)).to_f, false)
			hboth.pack_start(s1, true, true, 0)

			s2 = make_slider(obj, prop, c.eval(obj.get_property(prop)).to_f, true)	
			hboth.pack_start(s2, true, true, 0)
			
			vbox.pack_start(hboth, true, true, 0)
			
			hboth = Gtk::HBox.new(false, 3)
			t1 = Stock.chain_button do |but|
				if but.active?
					@linked << s1
				else
					@linked.delete(s1)
				end
			end
			
			hboth.pack_start(t1, true, false, 0)

			t2 = Stock.chain_button	do |but|
				if but.active?
					@linked << s2
				else
					@linked.delete(s2)
				end
			end
						
			hboth.pack_start(t2, true, false, 0)			
			vbox.pack_start(hboth, false, false, 0)
			
			vbox.pack_start(from_lbl = center_label(''), false, true, 0)

			frame.show_all
		
			@content << frame

			container = {:act1 => s1, :act2 => s2, :widget => frame, :from_lbl => from_lbl, :to_lbl => to_lbl, :frame => frame}
			container.merge!(state)

			super(obj, prop, container)
			update_range(obj, container)
		end
	
		def signal_do_pause_simulation
		end
	
		def signal_do_continue_simulation
		end
	end
end
