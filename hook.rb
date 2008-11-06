require 'graph'
require 'stock'
require 'objectlist'

module Cpg
	class Hook < Gtk::Window
		type_register
	
		def initialize(h = {})
			super(h)
			
			@vbox_main = Gtk::VBox.new(false, 0)
			@vbox_content = Gtk::VBox.new(false, 3)
			
			@uimanager = Gtk::UIManager.new
			ag = Gtk::ActionGroup.new('NormalActions')
			
			ag.add_actions([
				['FileMenuAction', nil, '_File', nil, nil, nil],
				['CloseAction', Gtk::Stock::CLOSE, nil, nil, nil, Proc.new { |g, a| destroy }],
				['ViewMenuAction', nil, '_View', nil, nil, nil]
			])
			
			ag.add_toggle_actions([
				['ViewSelectAction', nil, 'Show _object list', '<Control>o', 'Show/Hide property selection for monitoring', Proc.new { |g, a| select_toggled(a) }, true],

			])
			
			@uimanager.insert_action_group(ag, 0)
			@uimanager.add_ui(File.join(File.dirname(__FILE__), 'hook-ui.xml'))
			
			add_accel_group(@uimanager.accel_group)
			
			@vbox_main.pack_start(@uimanager.get_widget('/menubar'), false, false, 0)
			
			@paned = Gtk::HPaned.new
			create_objectlist

			@paned.pack1(content_area, true, true)			
			@vbox_content.pack_start(@paned)
			
			@vbox_main.pack_start(@vbox_content, true, true, 0)

			@map = {}
			@signals = {}
		
			add(@vbox_main)

			set_default_size(500, 400)
			@vbox_main.show_all
			
			signal_register(Application.instance.grid, 'object_removed') { |g, o| remove_hook(o) }
		end
		
		def create_objectlist
			@objectlist = ObjectList.new
			
			sw = Gtk::ScrolledWindow.new
			sw.set_policy(Gtk::POLICY_AUTOMATIC, Gtk::POLICY_AUTOMATIC)
			sw.add(@objectlist)
			
			sw.set_shadow_type(Gtk::ShadowType::ETCHED_IN)

			sw.show_all
			@paned.pack2(sw, false, false)

			@objectlist.signal_connect('toggled') do |lst, obj, prop|
				if @objectlist.active?(obj, prop)
					add_hook(obj, prop)
				else
					remove_hook(obj, prop)
				end
			end
			
			@objectlist.signal_connect('property_added') do |lst, obj, prop, piter|
				piter[2] = has_hook?(obj, prop) ? 1 : 0
			end
			
			@objlstsw = sw
		end
		
		def signals_unregister(obj)
			@signals[obj].each { |x| obj.signal_handler_disconnect(x) } if @signals.include?(obj)
			@signals.delete(obj)
		end
		
		def signal_register(obj, signal)
			@signals[obj] ||= []
			@signals[obj] << obj.signal_connect(signal) { |*args| yield *args }
		end
		
		def content_area
			# should override
			Gtk::VBox.new(false, 3)
		end
	
		def signal_do_destroy
			super
			
			@signals.keys.each { |obj| signals_unregister(obj) }
			@map.each { |o,v| remove_hook(o) }
		end
		
		def sort_hooks(a, b)
			0
		end
		
		def each_hook
			vals = []
			@map.each do |obj, properties|
				properties.each do |container|
					vals << [obj, container]
				end
			end
			
			# sort based on position
			vals.sort! { |a, b| sort_hook(a, b) }
			vals.each { |x| yield x[0], x[1][:prop] }
		end
		
		def has_hook?(obj, prop = nil)
			return false unless @map.include?(obj)
			return true if prop == nil

			@map[obj].any? { |p| p[:prop] == prop }
		end
		
		def add_hook_real(obj, prop, state)
			@map[obj] << state
		end
		
		def install_object(obj, prop)
			@map[obj] = []

			signal_register(obj, 'property_removed') do |o,p|
				remove_hook(o, p)
			end
		end
	
		def add_hook(obj, prop)
			if not has_hook?(obj)
				install_object(obj, prop)
			end
			
			# check if hook already present
			return false if has_hook?(obj, prop)
			
			add_hook_real(obj, prop, {:prop => prop})
			@objectlist.set_active(obj, prop, true) if @objectlist
		end
		
		def property_name(obj, prop, long=false)
			s = "#{obj}.#{prop}"
			
			if obj.is_a?(Components::Link) && long
				s << " #{obj.from} >> #{obj.to}"
			end
			
			s
		end
		
		def remove_hook_real(obj, container)
			container[:widget].destroy unless (!container[:widget] || container[:widget].destroyed?)
			@map[obj].delete(container)
		end
		
		def remove_hook(obj, prop = nil)
			return unless has_hook?(obj, prop)
			
			@map[obj].dup.each do |x| 
				if prop == nil || x[:prop] == prop
					remove_hook_real(obj, x)
				end
			end

			if @map[obj].empty?
				signals_unregister(obj)
				@map.delete(obj)
			end
			
			@objectlist.set_active(obj, prop, false) if @objectlist
		end
		
		def signal_do_key_press_event(ev)
			super

			if ev.keyval == Gdk::Keyval::GDK_Escape
				destroy
				true
			else
				false
			end
		end
		
		def signal_do_configure_event(ev)
			ret = super
			return ret if @configured
			
			@paned.position = allocation.width - 150
			@configured = true
		end
		
		def select_toggled(a)
			@objlstsw.visible = a.active?
			@paned.queue_draw
		end
	end
end
